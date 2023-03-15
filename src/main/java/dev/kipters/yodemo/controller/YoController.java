package dev.kipters.yodemo.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/yo")
public class YoController {
    
    @PostMapping
    public ResponseEntity<?> sendYo(String targetUser) {
        return ResponseEntity.ok(targetUser);
    }
}
